import { AuthProvider } from '../context_api/AuthContext/AuthProvider.jsx';
import AnimesProvider from '../context_api/AnimesContext/AnimesProvider.jsx';
import DtudoRouter from '../router/DtudoRouter.jsx';

export default function FullApp() {
    return (
        <AuthProvider>
            <AnimesProvider>
                <DtudoRouter />
            </AnimesProvider>
        </AuthProvider>
    );
}

