export interface PhotoForModeration {
    id: number;
    userId: number;
    userKnownAs: string;
    url: string;
    description: string;
    dateAdded: Date;
    isMain: boolean;
    isApproved: boolean;
}
