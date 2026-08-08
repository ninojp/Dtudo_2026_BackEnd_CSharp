import CatalogAuthProvider from '../context_api/AuthContext/CatalogAuthProvider.jsx';
import AnimesProvider from '../context_api/AnimesContext/AnimesProvider.jsx';
import CatalogRouter from '../router/CatalogRouter.jsx';

export default function CatalogOnlyApp() {
    return (
        <CatalogAuthProvider>
            <AnimesProvider>
                <CatalogRouter />
            </AnimesProvider>
        </CatalogAuthProvider>
    );
}
