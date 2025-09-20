/* eslint-disable @typescript-eslint/no-explicit-any */
import { getObjectImagesSuccess, ObjectImage, sceneSplitReducer, SceneSplitState } from "..";

describe('sceneSplitReducer', () => {
    const initialState: SceneSplitState = {
        objectImages: [],
    };

    it('should return the initial state when an unknown action is provided', () => {
        const action = { type: 'UNKNOWN' } as any;
        const state = sceneSplitReducer(initialState, action);
        expect(state).toBe(initialState);
    });

    it('should update objectImages on getObjectImagesSuccess', () => {
        const images: ObjectImage[] = [
            { imageUrl: 'url1', description: 'desc1' },
            { imageUrl: 'url2', description: 'desc2' }
        ];
        const action = getObjectImagesSuccess({ images });
        const state = sceneSplitReducer(initialState, action);
        expect(state.objectImages).toEqual(images);
    });

    it('should not mutate the previous state', () => {
        const images: ObjectImage[] = [{ imageUrl: 'url', description: 'desc' }];
        const action = getObjectImagesSuccess({ images });
        const state = sceneSplitReducer(initialState, action);
        expect(state).not.toBe(initialState);
        expect(initialState.objectImages).toEqual([]);
    });
});