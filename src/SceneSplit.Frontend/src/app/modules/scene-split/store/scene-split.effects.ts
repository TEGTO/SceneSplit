import { Injectable } from "@angular/core";
import { Actions, createEffect, ofType } from "@ngrx/effects";
import { catchError, map, mergeMap, of, switchMap } from "rxjs";
import { BookTemplateApiService, getObjectImages, getObjectImagesFailure, getObjectImagesSuccess, sendSceneImageFile, sendSceneImageFileFailure, sendSceneImageFileSuccess } from "..";
import { RedirectorService, SnackbarManager } from "../../shared";

@Injectable({
    providedIn: 'root'
})
export class BookEffects {
    constructor(
        private readonly actions$: Actions,
        private readonly apiService: BookTemplateApiService,
        private readonly snackbarManager: SnackbarManager,
        private readonly redirector: RedirectorService,
    ) { }

    getObjectImages$ = createEffect(() =>
        this.actions$.pipe(
            ofType(getObjectImages),
            switchMap(() =>
                this.apiService.getBooks().pipe(
                    map((response) => {
                        return getObjectImagesSuccess({ books: response });
                    }),
                    catchError(error => of(getObjectImagesFailure({ error: error.message })))
                )
            )
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
                this.apiService.createBook(action.req).pipe(
                    map((response) => {
                        return sendSceneImageFileSuccess({ book: response });
                    }),
                    catchError(error => of(sendSceneImageFileFailure({ error: error.message })))
                )
            )
        )
    );
}