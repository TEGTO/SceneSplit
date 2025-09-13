import { createFeatureSelector, createSelector } from "@ngrx/store";
import { ObjectImage, SceneSplitState } from "..";

export const selectSceneSplitState = createFeatureSelector<SceneSplitState>('scene-split');

export const selectObjectImages = createSelector(
    selectSceneSplitState,
    (state: SceneSplitState): ObjectImage[] => state.objectImages
);
