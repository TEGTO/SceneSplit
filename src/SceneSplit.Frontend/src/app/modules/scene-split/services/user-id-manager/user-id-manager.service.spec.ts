import { TestBed } from '@angular/core/testing';
import { UserIdManagerService } from './user-id-manager.service';

describe('UserIdManagerService', () => {
  let service: UserIdManagerService;

  const store: Record<string, string> = {};
  const mockLocalStorage = {
    getItem: (key: string): string | null => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value; },
    removeItem: (key: string) => { delete store[key]; },
    clear: () => { Object.keys(store).forEach(k => delete store[k]); },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(UserIdManagerService);

    spyOn(localStorage, 'getItem').and.callFake(mockLocalStorage.getItem);
    spyOn(localStorage, 'setItem').and.callFake(mockLocalStorage.setItem);
    spyOn(localStorage, 'removeItem').and.callFake(mockLocalStorage.removeItem);

    mockLocalStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should generate and store a new userId if none exists', () => {
    const userId = service.getUserId();
    expect(userId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/
    );
    expect(localStorage.setItem).toHaveBeenCalledWith('scene-split-user-id', userId);
  });

  it('should return existing userId if already stored', () => {
    const existingId = '1234-5678-9012';
    mockLocalStorage.setItem('scene-split-user-id', existingId);

    const userId = service.getUserId();
    expect(userId).toBe(existingId);
    expect(localStorage.setItem).not.toHaveBeenCalled();
  });

  it('should clear stored userId', () => {
    mockLocalStorage.setItem('scene-split-user-id', 'abc');
    service.clearUserId();
    expect(localStorage.removeItem).toHaveBeenCalledWith('scene-split-user-id');
    expect(mockLocalStorage.getItem('scene-split-user-id')).toBeNull();
  });

  it('should generate unique userIds on consecutive calls if storage is cleared', () => {
    const firstId = service.getUserId();
    service.clearUserId();
    const secondId = service.getUserId();

    expect(firstId).not.toBe(secondId);
  });
});