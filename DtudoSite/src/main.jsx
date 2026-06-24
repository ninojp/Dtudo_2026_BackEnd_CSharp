import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './main.css'
import AnimexObjsListProvider from './context_api/AnimexObjsListContext/AnimexObjsListProvider';
import ScrollToTop from './components/ScrollToTop/ScrollToTop.jsx';
import DtudoRouter from './router/DtudoRouter.jsx';
import { AuthProvider } from './context_api/AuthContext/AuthProvider.jsx';
import MyAnimesObjsListProvider from './context_api/MyAnimesObjsListContext/MyAnimesObjsListProvider.jsx';
import AnimesObjsListDetalhesProvider from './context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListProvider.jsx';
import MyAnimesBDLocalProvider from './context_api/MyAnimesBDLocalContext/MyAnimesBDLocalProvider.jsx';

createRoot(document.getElementById('root')).render(
    <StrictMode>
        <AuthProvider>
            <MyAnimesObjsListProvider>
                <AnimesObjsListDetalhesProvider>
                    <MyAnimesBDLocalProvider>
                        <AnimexObjsListProvider>
                            <DtudoRouter >
                                <ScrollToTop />
                            </DtudoRouter>
                        </AnimexObjsListProvider>
                    </MyAnimesBDLocalProvider>
                </AnimesObjsListDetalhesProvider>
            </MyAnimesObjsListProvider>
        </AuthProvider>
    </StrictMode>
);
