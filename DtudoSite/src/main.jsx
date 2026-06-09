import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.css'
import AnimexObjsListProvider from './context_api/AnimexObjsListContext/AnimexObjsListProvider';
import ScrollToTop from './components/ScrollToTop/ScrollToTop.jsx';
import DtudoRouter from './router/DtudoRouter.jsx';
import { AuthProvider } from './context_api/AuthContext/AuthProvider.jsx';
import MyAnimesObjsListProvider from './context_api/MyAnimesObjsListContext/MyAnimesObjsListProvider.jsx';
import AnimesObjsListDetalhesProvider from './context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListProvider.jsx';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <AuthProvider>
            <MyAnimesObjsListProvider>
                <AnimesObjsListDetalhesProvider>
                    <AnimexObjsListProvider>
                        <DtudoRouter >
                            <ScrollToTop />
                        </DtudoRouter>
                    </AnimexObjsListProvider>
                </AnimesObjsListDetalhesProvider>
            </MyAnimesObjsListProvider>
        </AuthProvider>
    </StrictMode>
);
