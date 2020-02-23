import {
  WEB3_INITIALIZED,
  catalystRealmInitialized,
  initCatalystRealm,
  setCatalystCandidates,
  setAddedCatalystCandidates,
  setContentWhitelist
} from './actions'
import { call, put, takeEvery, take, select } from 'redux-saga/effects'
import { pickCatalystRealm, fecthCatalystRealms, fetchCatalystStatuses } from './index'
import { Realm, Candidate } from './types'
import { META_CONFIGURATION_INITIALIZED } from '../meta/actions'
import { getAddedServers, isMetaConfigurationInitiazed, getContentWhitelist } from '../meta/selectors'
import { getRealmFromString } from '.'
import { REALM } from 'config'
import { getAllCatalystCandidates } from './selectors'

export function* daoSaga(): any {
  yield takeEvery(WEB3_INITIALIZED, loadCatalystRealms)
}

function* loadCatalystRealms() {
  if (!(yield select(isMetaConfigurationInitiazed))) {
    yield take(META_CONFIGURATION_INITIALIZED)
  }

  const candidates: Candidate[] = yield call(fecthCatalystRealms)

  yield put(setCatalystCandidates(candidates))

  const added: string[] = yield select(getAddedServers)
  const addedCandidates: Candidate[] = yield call(fetchCatalystStatuses, added.map(url => ({ domain: url })))

  yield put(setAddedCatalystCandidates(addedCandidates))

  const allCandidates: Candidate[] = yield select(getAllCatalystCandidates)

  const whitelist: string[] = yield select(getContentWhitelist)
  let whitelistedCandidates = allCandidates.filter(candidate => whitelist.includes(candidate.domain))
  if (whitelistedCandidates.length === 0) {
    // if intersection is empty (no whitelisted or not in our candidate set) => whitelist all candidates
    whitelistedCandidates = allCandidates
  }

  yield put(setContentWhitelist(whitelistedCandidates))

  let realm: Realm = yield call(getConfiguredRealm, allCandidates)
  if (!realm) {
    realm = yield call(pickCatalystRealm, allCandidates)
  }

  yield put(initCatalystRealm(realm))

  yield put(catalystRealmInitialized())
}

function getConfiguredRealm(candidates: Candidate[]) {
  if (REALM) {
    return getRealmFromString(REALM, candidates)
  }
}
