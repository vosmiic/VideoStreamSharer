export interface IHome {
    RoomNames : IRoomName[]
}

export interface IRoomName {
    Id : string,
    Name : string,
    VisitDateTime : Date
}