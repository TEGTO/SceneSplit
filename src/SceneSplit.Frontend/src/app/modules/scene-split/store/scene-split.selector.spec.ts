import { ObjectImage, SceneSplitState, selectObjectImages, selectSceneSplitState } from "..";

describe('SceneSplit Selectors', () => {
    const initialState: { 'scene-split': SceneSplitState } = {
        'scene-split': {
            objectImages: [],
        },
    };

    it('selectSceneSplitState should return the feature state', () => {
        const result = selectSceneSplitState.projector(initialState['scene-split']);
        expect(result).toEqual(initialState['scene-split']);
    });

    it('selectObjectImages should return objectImages from state', () => {
        const images: ObjectImage[] = [
            { imageUrl: 'url1', description: 'desc1' },
            { imageUrl: 'url2', description: 'desc2' },
        ];

        const state: SceneSplitState = { objectImages: images };

        const result = selectObjectImages.projector(state);
        expect(result).toEqual(images);
    });

    it('selectObjectImages should return empty array if no images', () => {
        const state: SceneSplitState = { objectImages: [] };
        const result = selectObjectImages.projector(state);
        expect(result).toEqual([]);
    });
});