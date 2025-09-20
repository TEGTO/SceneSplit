import { ObjectImage } from '..';
import * as SceneSplitActions from './scene-split.actions';

describe('SceneSplitActions', () => {
    it('getObjectImages should create the correct action', () => {
        const action = SceneSplitActions.getObjectImages();
        expect(action.type).toBe('[Scene Split] Get Object Images');
    });

    it('getObjectImagesSuccess should create the correct action with payload', () => {
        const images: ObjectImage[] = [{ imageUrl: 'url', description: 'desc' }];
        const action = SceneSplitActions.getObjectImagesSuccess({ images });
        expect(action.type).toBe('[Scene Split] Get Object Images Success');
        expect(action.images).toBe(images);
    });

    it('getObjectImagesFailure should create the correct action with error', () => {
        const error = new Error('fail');
        const action = SceneSplitActions.getObjectImagesFailure({ error });
        expect(action.type).toBe('[Scene Split] Get Object Images Failure');
        expect(action.error).toBe(error);
    });

    it('sendSceneImageFile should create the correct action with file', () => {
        const file = new File([''], 'test.png', { type: 'image/png' });
        const action = SceneSplitActions.sendSceneImageFile({ file });
        expect(action.type).toBe('[Scene Split] Send Scene Image File');
        expect(action.file).toBe(file);
    });

    it('sendSceneImageFileSuccess should create the correct action', () => {
        const action = SceneSplitActions.sendSceneImageFileSuccess();
        expect(action.type).toBe('[Scene Split] Send Scene Image File Success');
    });

    it('sendSceneImageFileFailure should create the correct action with error', () => {
        const error = 'failed';
        const action = SceneSplitActions.sendSceneImageFileFailure({ error });
        expect(action.type).toBe('[Scene Split] Send Scene Image File Failure');
        expect(action.error).toBe(error);
    });
});
