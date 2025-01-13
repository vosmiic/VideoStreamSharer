import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import Layout from "../layout.tsx";
import Router from "./Router.tsx";
import {AuthProvider, AuthProviderProps} from "react-oidc-context";
import {WebStorageStateStore} from "oidc-client-ts";
const test : AuthProviderProps = {
    authority: "https://localhost:7154",
    client_id: "Default",
    redirect_uri: "https://localhost:7154/authentication/login-callback",
    //userStore: new WebStorageStateStore({ store: window.localStorage })
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <AuthProvider {...test}>
          <Layout>
            <Router />
          </Layout>
      </AuthProvider>
  </StrictMode>,
)
