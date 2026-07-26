import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.css'
import DtudoRouter from './router/DtudoRouter.jsx';
import { AuthProvider } from './context_api/AuthContext/AuthProvider.jsx';
import AnimesProvider from './context_api/AnimesContext/AnimesProvider.jsx';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <AuthProvider>
            <AnimesProvider>
                <DtudoRouter />
            </AnimesProvider>
        </AuthProvider>
    </StrictMode>
);
