import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.css'
import DtudoRouter from './router/DtudoRouter.jsx';
import { AuthProvider } from './context_api/AuthContext/AuthProvider.jsx';
import AnimesObjsListDetalhesProvider from './context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListProvider.jsx';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <AuthProvider>
            <AnimesObjsListDetalhesProvider>
                <DtudoRouter />
            </AnimesObjsListDetalhesProvider>
        </AuthProvider>
    </StrictMode>
);
