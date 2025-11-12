export default class QueueAddBody {
    RoomId: string;
    Url: string;

    constructor(roomId : string, url: string) {
        this.RoomId = roomId;
        this.Url = url;
    }
}