import {Context, createContext} from "react";
import {HubConnection} from "@microsoft/signalr";

export const HubContext : Context<HubConnection> = createContext();