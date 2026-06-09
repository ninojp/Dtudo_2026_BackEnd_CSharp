import Spinner from '../../components/Spinner/Spinner';
import { useAuth } from '../../hooks/useAuth';

import AuthContext from './AuthContext';

export const AuthProvider = ({ children }) => {
    const auth = useAuth(); // useAuth é chamado aqui, uma única vez.
    // Enquanto carrega o usuário do localStorage, pode exibir um spinner
    if (auth.isLoading) {
        return <Spinner />;
    };
    //======================================
    return (
        <AuthContext.Provider value={auth}>
            {children}
        </AuthContext.Provider>
    );
};
// Explicação Detalhada
// AuthContext: É o objeto criado com createContext(). Ele funciona como um "canal" para transportar os dados.
// AuthProvider: É o seu componente que usa <AuthContext.Provider> para colocar os dados (o valor de auth) dentro desse "canal".
// useContext(AuthContext): É como um componente "escuta" o que está sendo transportado pelo canal AuthContext para poder usar os dados.
