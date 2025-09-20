/* eslint-disable @typescript-eslint/no-explicit-any */
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private config: any;

  async load(): Promise<void> {
    const response = await fetch('/assets/config.json');
    this.config = await response.json();
  }

  get hubUrl(): string {
    return this.config?.hubUrl;
  }

  get maxSizeFile(): number {
    return this.config?.maxFileSize.valueOf() || 10 * 1024 * 1024;
  }

  get allowedImageTypes(): string[] {
    return this.config?.allowedImageTypes.split(',') || [];
  }
}
