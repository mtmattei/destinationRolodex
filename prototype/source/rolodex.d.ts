export interface DestinationMedia {
  type?: 'image' | 'video';
  src: string;
  /** A still image for video loading and reduced-motion mode. */
  poster?: string;
  /** Small image for this destination, displayed while its full image loads. */
  preview?: string;
  /** CSS object-position, for example '50% 45%'. */
  position?: string;
  alt?: string;
  credit?: string;
  source?: string;
}
export interface Destination {
  id?: string;
  country: string;
  location?: string;
  region?: string;
  description?: string;
  color?: string;
  media: DestinationMedia;
}
export interface DestinationChangeDetail {
  index: number;
  id: string;
  country: string;
  destination: Destination;
}
export type DestinationTransition = 'morph' | 'liquid';
export interface DestinationRolodexElement extends HTMLElement {
  transition: DestinationTransition;
  /** Milliseconds; clamped to 350–3000. */
  transitionDuration: number;
  /** Displacement intensity; clamped to 0–1. */
  transitionStrength: number;
  readonly transitionRenderer: 'webgl' | 'crossfade';
  replayTransition(): Promise<boolean>;
  seekTransition(progress: number): Promise<boolean>;
  destinations: Destination[];
  readonly selectedIndex: number;
  readonly selectedDestination: Destination | null;
  readonly view: 'browse' | 'look';
  readonly playing: boolean;
  next(): void;
  previous(): void;
  goTo(index: number): void;
  play(): void;
  pause(): void;
  setView(view: 'browse' | 'look'): void;
}
declare global {
  interface HTMLElementTagNameMap {
    'destination-rolodex': DestinationRolodexElement;
  }
}
