import {StrictMode} from 'react'
import {createRoot} from 'react-dom/client'
import './index.css'
import Layout from "../layout.tsx";
import Router from "./Router.tsx";

createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <Layout>
          <Router />
      </Layout>
  </StrictMode>,
)
