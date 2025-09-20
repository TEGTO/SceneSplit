import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UserIdManagerService {
  private readonly STORAGE_KEY = 'scene-split-user-id';

  getUserId(): string {
    let userId = localStorage.getItem(this.STORAGE_KEY);

    if (!userId) {
      userId = this.generateUserId();
      localStorage.setItem(this.STORAGE_KEY, userId);
    }

    return userId;
  }

  clearUserId() {
    localStorage.removeItem(this.STORAGE_KEY);
  }

  private generateUserId(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }
}