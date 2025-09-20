/* eslint-disable @typescript-eslint/no-explicit-any */
import { createAction, props } from "@ngrx/store";
import { ObjectImage } from "..";

export const getObjectImages = createAction(
    '[Scene Split] Get Object Images'
);
export const getObjectImagesSuccess = createAction(
    '[Scene Split] Get Object Images Success',
    props<{ images: ObjectImage[] }>()
);
export const getObjectImagesFailure = createAction(
    '[Scene Split] Get Object Images Failure',
    props<{ error: any }>()
);

export const sendSceneImageFile = createAction(
    '[Scene Split] Send Scene Image File',
    props<{ file: File }>()
);
export const sendSceneImageFileSuccess = createAction(
    '[Scene Split] Send Scene Image File Success'
);
export const sendSceneImageFileFailure = createAction(
    '[Scene Split] Send Scene Image File Failure',
    props<{ error: any }>()
);