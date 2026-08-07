import Spinner from '../../components/Spinner/Spinner';
import { useAuth } from '../../hooks/useAuth';

import AuthContext from './AuthContext';

export const AuthProvider = ({ children }) => {
    const auth = useAuth();
    if (auth.isLoading) {
        return <Spinner />;
    };

    return (
        <AuthContext.Provider value={auth}>
            {children}
        </AuthContext.Provider>
    );
};
    // useContext(AuthContext): É como um componente "escuta" o que está sendo transportado pelo canal AuthContext para poder usar os dados.
