declare namespace TelemetryGitHub {

  export interface IAppConfiguration {
    initialReportDelayInMs: number;
  }

  export interface ISettings {
    getItem(key: string): Promise<string | undefined>;
    setItem(key: string, value: string): Promise<void>;
  }

  export interface IDimensions {
    /** The app version. */
    appVersion: string;

    /** the platform */
    platform: string;

    /** The install ID. */
    guid: string;

    /** The date the metrics were recorded, in ISO-8601 format */
    date: string;

    lang: string;

    gitHubUser: string | undefined;
  }

  export interface IMetrics {
    readonly eventType: "usage";

    dimensions: IDimensions;
    // metrics names are defined by the client and thus aren't knowable
    // at compile time here.
    measures: any;

    // array of custom events that can be defined by the client
    customEvents: any[];

    // array of timing events
    timings: any[];
  }

  export interface ICounter {
    name: string;
    count: number;
  }

  export interface IStatsDatabase {
    close(): Promise<void>;
    incrementCounter(instanceId: string, counterName: string): Promise<void>;
    clearData(date: Date): Promise<void>;
    addCustomEvent(instanceId: string, eventType: string, customEvent: any): Promise<void>;
    addTiming(instanceId: string, eventType: string, durationInMilliseconds: number, metadata: any): Promise<void>;
    getMetrics(beforeDate?: Date): Promise<IMetrics[]>;
    getCurrentMetrics(instanceId: string): Promise<IMetrics>;
  }

  export const enum AppName {
    Atom = "atom",
    VSCode = "vscode",
  }

  export class StatsStore {
    constructor(
      appName: AppName,
      version: string,
      getAccessToken?: () => string,
      settings?: ISettings,
      database?: IStatsDatabase,
      configuration?: IAppConfiguration
    );

    /** Set the username to send along with the metrics (optional) */
    setGitHubUser(gitHubUser: string): void;

    /** Are we running in development mode? */
    setDevMode(isDevMode: boolean): void;

    /** Disable storing metrics when in development mode.
     * The default is false because the default backend is localStorage,
     * which cannot distinguish between dev and non-dev mode when saving
     * metrics. If you supply a backend that can store dev and release metrics
     * in different places, set this to true
     */
    setTrackInDevMode(track: boolean): void;

    /** Shutdown the data store, if the backend supports it */
    shutdown(): Promise<void>;
    /** Set whether the user has opted out of stats reporting. */
    setOptOut(optOut: boolean): Promise<void>;
    reportStats(getDate: () => string): Promise<void>;

    addCustomEvent(eventType: string, event: any): Promise<void>;
    /**
     * Add timing data to the stats store, to be sent with the daily metrics requests.
     */
    addTiming(eventType: string, durationInMilliseconds: number, metadata?: {}): Promise<void>;
    /**
     * Increment a counter.  This is used to track usage statistics.
     */
    incrementCounter(counterName: string): Promise<void>;

    /** Helper method to create a new empty report for the current day */
    createReport(): IMetrics;
  }

  export function getYearMonthDay(date: Date): number;
}

declare module "telemetry-github" {
  export = TelemetryGitHub;
}