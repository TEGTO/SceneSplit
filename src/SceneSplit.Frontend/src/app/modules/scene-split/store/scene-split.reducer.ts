import { createReducer, on } from "@ngrx/store";
import { getObjectImagesSuccess, ObjectImage } from "..";

export interface SceneSplitState {
    objectImages: ObjectImage[],
}
const initialSceneSplitState: SceneSplitState = {
    objectImages: [],
};

export const sceneSplitReducer = createReducer(
    initialSceneSplitState,

    on(getObjectImagesSuccess, (state, { images }) => ({
        ...state,
        objectImages: images,
    })),
);