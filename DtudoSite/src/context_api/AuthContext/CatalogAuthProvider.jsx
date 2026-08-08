import AuthContext from './AuthContext';

const publicCatalogAuth = {
    isAuthenticated: false,
    isLoading: false,
    error: null,
};

export default function CatalogAuthProvider({ children }) {
    return (
        <AuthContext.Provider value={publicCatalogAuth}>
            {children}
        </AuthContext.Provider>
    );
}

