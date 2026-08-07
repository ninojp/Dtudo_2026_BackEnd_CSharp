import { useCallback, useEffect, useState } from 'react';
import {
    BffRequestError,
    buildBffUrl,
    getSafeReturnPath,
    requestBff,
} from '../services/bffClient';

const SESSION_EXPIRED_MESSAGE = 'Sua sessao expirou ou foi revogada.';
const SESSION_CHECK_ERROR = 'Nao foi possivel verificar a sessao.';

function isAuthorizationFailure(error) {
    return error instanceof BffRequestError
        && (error.status === 401 || error.status === 403);
}

function getErrorMessage(error, fallback) {
    return error instanceof Error && error.message ? error.message : fallback;
}

export const useAuth = () => {
    const [user, setUser] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);

    const refreshSession = useCallback(async (signal) => {
        try {
            const session = await requestBff('/bff/me', { signal });
            const nextUser = session?.authenticated && session.user ? session.user : null;
            setUser(nextUser);
            setError(null);
            return nextUser;
        } catch (requestError) {
            if (requestError.name === 'AbortError') {
                throw requestError;
            }

            setUser(null);
            if (isAuthorizationFailure(requestError)) {
                setError(null);
            } else {
                setError(SESSION_CHECK_ERROR);
            }
            return null;
        } finally {
            if (!signal?.aborted) {
                setIsLoading(false);
            }
        }
    }, []);

    useEffect(() => {
        const controller = new AbortController();
        refreshSession(controller.signal).catch(() => undefined);

        return () => controller.abort();
    }, [refreshSession]);

    useEffect(() => {
        const handleSessionExpired = () => {
            setUser(null);
            setError(SESSION_EXPIRED_MESSAGE);
        };

        window.addEventListener('dtudo:bff-session-expired', handleSessionExpired);
        return () => window.removeEventListener('dtudo:bff-session-expired', handleSessionExpired);
    }, []);

    const login = useCallback((returnUrl = '/') => {
        const safeReturnPath = getSafeReturnPath(returnUrl);
        window.location.assign(buildBffUrl(
            `/bff/login?returnUrl=${encodeURIComponent(safeReturnPath)}`,
        ));
    }, []);

    const logout = useCallback(async (returnUrl = '/auth/login') => {
        setIsLoading(true);
        setError(null);

        try {
            const antiforgery = await requestBff('/bff/antiforgery');
            if (!antiforgery?.token) {
                throw new Error('Protecao contra requisicoes falsificadas indisponivel.');
            }

            await requestBff(
                `/bff/logout?returnUrl=${encodeURIComponent(getSafeReturnPath(returnUrl))}`,
                {
                    method: 'POST',
                    headers: {
                        'X-CSRF-TOKEN': antiforgery.token,
                    },
                },
            );
            setUser(null);
            return { success: true };
        } catch (requestError) {
            if (isAuthorizationFailure(requestError)) {
                setUser(null);
                setError(null);
                return { success: true };
            }

            const message = getErrorMessage(requestError, 'Nao foi possivel encerrar a sessao.');
            setError(message);
            return { success: false, error: message };
        } finally {
            setIsLoading(false);
        }
    }, []);

    return {
        user,
        isLoading,
        error,
        isAuthenticated: !!user,
        login,
        logout,
        refreshSession,
    };
};
