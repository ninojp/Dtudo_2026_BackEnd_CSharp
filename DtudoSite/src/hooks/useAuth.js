import { useState, useEffect } from 'react'

const API_AUTH_BASE_URL = (
    import.meta.env.VITE_API_AUTH_BASE_URL
    || import.meta.env.VITE_API_LOCAL_MYANIMES_BASE_URL
    || 'https://localhost:63980/'
).replace(/\/$/, '')

const AUTH_USER_KEY = 'auth_user'
const AUTH_TOKEN_KEY = 'auth_token'

const requestJson = async (path, payload) => {
    const response = await fetch(`${API_AUTH_BASE_URL}${path}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
    })

    const data = await response.json().catch(() => null)
    if (!response.ok || !data?.success) {
        throw new Error(data?.message || 'Falha na autenticacao')
    }

    return data
}

export const useAuth = () => {
    const [user, setUser] = useState(null)
    const [isLoading, setIsLoading] = useState(true)

    useEffect(() => {
        const storedUser = localStorage.getItem(AUTH_USER_KEY)
        if (storedUser) {
            try {
                setUser(JSON.parse(storedUser))
            } catch (error) {
                console.error('Erro ao carregar usuario do localStorage:', error)
                localStorage.removeItem(AUTH_USER_KEY)
                localStorage.removeItem(AUTH_TOKEN_KEY)
            }
        }
        setIsLoading(false)
    }, [])

    const persistAuth = (authResponse) => {
        setUser(authResponse.user)
        localStorage.setItem(AUTH_USER_KEY, JSON.stringify(authResponse.user))
        if (authResponse.token) {
            localStorage.setItem(AUTH_TOKEN_KEY, authResponse.token)
        }
    }

    const register = async (name, email, password) => {
        try {
            const response = await requestJson('/apiLocal/Auth/register', { name, email, password })
            persistAuth(response)
            return { success: true, user: response.user }
        } catch (error) {
            return { success: false, error: error.message }
        }
    }

    const login = async (email, password) => {
        try {
            const response = await requestJson('/apiLocal/Auth/login', { login: email, password })
            persistAuth(response)
            return { success: true, user: response.user }
        } catch (error) {
            return { success: false, error: error.message }
        }
    }

    const logout = () => {
        setUser(null)
        localStorage.removeItem(AUTH_USER_KEY)
        localStorage.removeItem(AUTH_TOKEN_KEY)
    }

    return {
        user,
        isLoading,
        isAuthenticated: !!user,
        register,
        login,
        logout
    }
}
