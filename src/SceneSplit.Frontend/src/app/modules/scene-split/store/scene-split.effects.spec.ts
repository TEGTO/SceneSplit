/* eslint-disable @typescript-eslint/no-explicit-any */
import { fakeAsync, TestBed } from "@angular/core/testing";
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable, Subject } from "rxjs";
import { getObjectImages, getObjectImagesFailure, getObjectImagesSuccess, ObjectImage, SceneSplitEffects, SceneSplitHubClientService, sendSceneImageFile, sendSceneImageFileFailure, sendSceneImageFileSuccess, UserIdManagerService } from "..";
import { SnackbarManager } from "../../shared";

describe('SceneSplitEffects (no marbles)', () => {
    let actions$: Subject<any>;
    let effects: SceneSplitEffects;

    let hubServiceMock: jasmine.SpyObj<SceneSplitHubClientService>;
    let snackbarMock: jasmine.SpyObj<SnackbarManager>;
    let userIdManagerMock: jasmine.SpyObj<UserIdManagerService>;

    beforeEach(() => {
        actions$ = new Subject();

        hubServiceMock = jasmine.createSpyObj('SceneSplitHubClientService', [
            'startConnection',
            'uploadSceneImage'
        ]);
        hubServiceMock.images$ = new Subject<ObjectImage[]>();
        hubServiceMock.errors$ = new Subject<Error>();

        snackbarMock = jasmine.createSpyObj('SnackbarManager', ['openErrorSnackbar']);
        userIdManagerMock = jasmine.createSpyObj('UserIdManagerService', ['getUserId']);
        userIdManagerMock.getUserId.and.returnValue('user-123');

        TestBed.configureTestingModule({
            providers: [
                SceneSplitEffects,
                provideMockActions(() => actions$ as Observable<any>),
                { provide: SceneSplitHubClientService, useValue: hubServiceMock },
                { provide: SnackbarManager, useValue: snackbarMock },
                { provide: UserIdManagerService, useValue: userIdManagerMock }
            ]
        });

        effects = TestBed.inject(SceneSplitEffects);
    });

    describe('getObjectImages$', () => {
        it('should dispatch success when hub emits images', (done) => {
            const images: ObjectImage[] = [
                { imageUrl: 'url1', description: 'img1' }
            ];

            effects.getObjectImages$.subscribe((action) => {
                expect(action).toEqual(getObjectImagesSuccess({ images }));
                done();
            });

            actions$.next(getObjectImages());
            (hubServiceMock.images$ as Subject<ObjectImage[]>).next(images);
        });

        it('should dispatch failure when hub emits error', (done) => {
            effects.getObjectImages$.subscribe((action) => {
                expect(action).toEqual(getObjectImagesFailure({ error: 'connection failed' }));
                done();
            });

            actions$.next(getObjectImages());
            (hubServiceMock.errors$ as Subject<Error>).next(new Error('connection failed'));
        });
    });

    describe('sendSceneImageFile$', () => {
        it('should dispatch success when upload succeeds', (done) => {
            hubServiceMock.uploadSceneImage.and.returnValue(Promise.resolve());

            const mockFile = new File(['data'], 'test.png', { type: 'image/png' });

            effects.sendSceneImageFile$.subscribe((action) => {
                expect(action).toEqual(sendSceneImageFileSuccess());
                done();
            });

            actions$.next(sendSceneImageFile({ file: mockFile }));
        });

        it('should dispatch failure when upload fails', (done) => {
            hubServiceMock.uploadSceneImage.and.returnValue(Promise.reject(new Error('upload failed')));
            const mockFile = new File(['data'], 'test.png', { type: 'image/png' });

            effects.sendSceneImageFile$.subscribe((action) => {
                expect(action).toEqual(sendSceneImageFileFailure({ error: 'upload failed' }));
                done();
            });

            actions$.next(sendSceneImageFile({ file: mockFile }));
        });
    });

    describe('sendSceneImageFileFailure$', () => {
        it('should call snackbar on failure', fakeAsync(() => {
            effects.sendSceneImageFileFailure$.subscribe(() => {
                expect(snackbarMock.openErrorSnackbar).toHaveBeenCalledWith([
                    'Failed to send scene image: boom'
                ]);
            });

            actions$.next(sendSceneImageFileFailure({ error: 'boom' }));
        }));
    });
});