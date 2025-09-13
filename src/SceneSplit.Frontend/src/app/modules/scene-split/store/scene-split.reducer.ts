import { createReducer, on } from "@ngrx/store";
import { getObjectImagesSuccess, ObjectImage, sendSceneImageFileSuccess } from "..";

export interface SceneSplitState {
    objectImages: ObjectImage[],
}
const initialSceneSplitState: SceneSplitState = {
    objectImages: [],
};

export const sceneSplitReducer = createReducer(
    initialSceneSplitState,

    on(getObjectImagesSuccess, (state, { books }) => ({
        ...state,
        objectImages: books,
    })),

    on(sendSceneImageFileSuccess, (state, { book }) => ({
        ...state,
        objectImages: [book, ...state.objectImages],
    })),

);