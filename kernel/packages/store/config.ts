/**
 * Configuration holds settings in four granularities. These are (with some non-exhaustive examples):
 *
 * - Build, values that are set at compilation time.
 *   * Environment target: Production/Development build
 * - Runtime, values set at the start of the execution that will not change
 *   * Ethereum network: Whether to download info from mainnet or ropsten
 *   * Overriden meta configuration: Skip some content servers
 * - Registered, volatile values based on observations/events experienced
 *   * Current content server
 *   * DDoS protection
 * - Preferences, user-selected values
 *   * Field of view
 *   * State of HUD displays (hide/show UI)
 */
export type Configuration = {
  build: {
    target: 'production' | 'staging' | 'development'
    entryPoint: 'builder' | 'preview' | 'world' | 'automation'
    interactionLimits: {
      clickDistance: number
    }
    /**
     * Limit the size and behavior of scripts inside a scene
     * (these are increased per-parcel)
     */
    parcelLimits: {
      entities: number
      height: number

      triangles: number
      bodies: number
      textures: number
      materials: number
      geometries: number
    }
    parcelSize: {
      sideLength: number
      halfLength: number
      centimeter: number
    }
  }
  runtime: {
    environment: 'org' | 'today' | 'zone' | 'localhost' | 'preview'
    ethereumNetwork: 'mainnet' | 'ropsten'
    metaConfig: undefined
  }
  registered: {
    realms: {
      candidateRealms: RealmDefinition[]
      realmsList: RealmListing[]
      invalidRealms: InvalidRealm[]
    }
    currentServers: {
      contentServerDomain: string
      contentServerURL: string
      commsServerDomain: string
      commsServerURL: string
    }
  }
  preferences: {
    hiddenSceneUI: Set<SceneId>
    lineOfSight: {
      loadRadius: number
      unloadRadius: number
    }
  }
}

type SceneId = string

type RealmDefinition = {
  serverDomain: string
  layerName: string
}

type InvalidRealm = RealmDefinition & {
  online: false
}

type RealmListing = RealmDefinition & {
  online: true
  currentUsers: number
  latency: number
}
