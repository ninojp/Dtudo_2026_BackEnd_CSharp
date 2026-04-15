import { useState, useEffect } from 'react'

const createUser = (name, email, password) => ({
    id: Date.now().toString(),
    name,
    email,
    password,
    createdAt: new Date().toISOString()
})
//============================
export const useAuth = () => {
    const [user, setUser] = useState(null)
    const [isLoading, setIsLoading] = useState(true)
    //----------------------------------------------
    useEffect(() => {
        const storedUser = localStorage.getItem('auth_user')
        if (storedUser) {
            try {
                setUser(JSON.parse(storedUser))
            } catch (error) {
                console.error('Erro ao carregar usuário do localStorage:', error)
                localStorage.removeItem('auth_user')
            }
        }
        setIsLoading(false)
    }, [])
    //-------------------------------------------
    const register = (name, email, password) => {
        try {
            const existingUsers = JSON.parse(localStorage.getItem('auth_users') || '[]')
            const userExists = existingUsers.find(u => u.email === email)
            if (userExists) {
                throw new Error('Usuário já existe com este email')
            }
            const newUser = createUser(name, email, password)
            existingUsers.push(newUser)
            localStorage.setItem('auth_users', JSON.stringify(existingUsers))
            setUser(newUser)
            localStorage.setItem('auth_user', JSON.stringify(newUser))
            return { success: true, user: newUser }
        } catch (error) {
            return { success: false, error: error.message }
        }
    }
    //----------------------------------
    const login = (email, password) => {
        try {
            const users = JSON.parse(localStorage.getItem('auth_users') || '[]')
            const user = users.find(u => u.email === email && u.password === password)
            if (!user) {
                throw new Error('Email ou senha incorretos')
            }
            setUser(user)
            localStorage.setItem('auth_user', JSON.stringify(user))
            return { success: true, user }
        } catch (error) {
            return { success: false, error: error.message }
        }
    }
    //-----------------------------
    const logout = () => {
        setUser(null)
        localStorage.removeItem('auth_user')
    }
    //-----------------------------
    const isAuthenticated = !!user
    //=============================
    return {
        user,
        isLoading,
        isAuthenticated,
        register,
        login,
        logout
    }
};
