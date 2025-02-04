import {StrictMode} from 'react'
import {createRoot} from 'react-dom/client'
import './index.css'
import Layout from "../layout.tsx";
import Router from "./Router.tsx";
import {HubContext} from "./Contexts/HubContext.tsx";
import {HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {API_URL} from "./constants.tsx";

const hubConnection = new HubConnectionBuilder()
    .withUrl(`${API_URL}/hub`)
    .build();

if (hubConnection.state == HubConnectionState.Disconnected) {
    hubConnection.start();
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <Layout>
          <HubContext.Provider value={hubConnection}>
              <Router />
          </HubContext.Provider>
      </Layout>
  </StrictMode>,
)
