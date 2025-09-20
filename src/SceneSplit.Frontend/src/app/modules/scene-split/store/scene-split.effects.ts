import { Injectable } from "@angular/core";
import { Actions, createEffect, ofType } from "@ngrx/effects";
import { catchError, from, map, merge, mergeMap, of, switchMap } from "rxjs";
import { getObjectImages, getObjectImagesFailure, getObjectImagesSuccess, SceneSplitHubClientService, sendSceneImageFile, sendSceneImageFileFailure, sendSceneImageFileSuccess, UserIdManagerService } from "..";
import { SnackbarManager } from "../../shared";

@Injectable({
    providedIn: 'root'
})
export class BookEffects {
    private readonly userId: string;

    constructor(
        private readonly actions$: Actions,
        private readonly snackbarManager: SnackbarManager,
        private readonly hubService: SceneSplitHubClientService,
        private readonly userIdManager: UserIdManagerService,
    ) {
        this.userId = this.userIdManager.getUserId();
    }

    getObjectImages$ = createEffect(() =>
        this.actions$.pipe(
            ofType(getObjectImages),
            switchMap(() => {
                this.hubService.startConnection(this.userId);

                return merge(
                    this.hubService.images$.pipe(
                        map(images => getObjectImagesSuccess({ images }))
                    ),
                    this.hubService.errors$.pipe(
                        map(error => getObjectImagesFailure({ error: error.message }))
                    )
                );
            })
        )
    );
    getObjectImagesFailure$ = createEffect(() =>
        this.actions$.pipe(
            ofType(getObjectImagesFailure),
            switchMap((action) => {
                this.snackbarManager.openErrorSnackbar(["Failed to get object images: " + action.error]);
                return of();
            })
        ),
        { dispatch: false }
    );

    sendSceneImageFile$ = createEffect(() =>
        this.actions$.pipe(
            ofType(sendSceneImageFile),
            mergeMap((action) =>
                from(this.hubService.uploadSceneImage(this.userId, action.file)).pipe(
                    map(() => sendSceneImageFileSuccess()),
                    catchError(error => of(sendSceneImageFileFailure({ error: error.message })))
                )
            )
        )
    );

    sendSceneImageFileFailure$ = createEffect(() =>
        this.actions$.pipe(
            ofType(sendSceneImageFileFailure),
            switchMap((action) => {
                this.snackbarManager.openErrorSnackbar(["Failed to send scene image: " + action.error]);
                return of();
            })
        ),
        { dispatch: false }
    );
}