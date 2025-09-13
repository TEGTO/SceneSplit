import { ObjectImage } from "../..";

export interface ObjectImageResponse {
    url: string;
    description: string;
}

export function mapObjectImageResponseToObjectImage(response: ObjectImageResponse): ObjectImage {
    return {
        url: response.url,
        description: response.description,
    };
}