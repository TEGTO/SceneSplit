import { ObjectImage } from "..";

export interface ObjectImageResponse {
    ImageUrl: string;
    Description: string;
}

export function mapObjectImageResponseToObjectImage(response: ObjectImageResponse): ObjectImage {
    return {
        imageUrl: response.ImageUrl,
        description: response.Description,
    };
}