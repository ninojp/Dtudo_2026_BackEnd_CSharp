import { BrowserRouter, Route, Routes } from 'react-router-dom';
import CatalogIndexLayout from '../layouts/CatalogIndexLayout/CatalogIndexLayout';
import NotFound from '../pages/NotFound/NotFound';
import Animes from '../pages/Animes/Animes';
import AnimesDetalhes from '../pages/Animes/AnimesDetalhes/AnimesDetalhes';
import AnimesRelacionados from '../pages/Animes/AnimesRelacionados/AnimesRelacionados';

export default function CatalogRouter() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<CatalogIndexLayout />}>
                    <Route index element={<Animes />} />
                    <Route path="animes">
                        <Route index element={<Animes />} />
                        <Route path="animes-detalhes/:malId" element={<AnimesDetalhes />} />
                        <Route path="animes-relacionados/:malId" element={<AnimesRelacionados />} />
                    </Route>
                    <Route path="*" element={<NotFound />} />
                </Route>
            </Routes>
        </BrowserRouter>
    );
}
