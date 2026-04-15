import styles from './register.module.css'
import { Link, useNavigate } from "react-router"
import { use } from 'react';
import AuthContext from '../../context_api/AuthContext/AuthContext';

export default function Register () {
    const { register } = use(AuthContext);
    const navigate = useNavigate();

    const onSubmitForm = (formData) => {
        const name = formData.get('name');
        const email = formData.get('email');
        const password = formData.get('password');
        const response = register(name, email, password);
        if (response.success) {
            navigate('/auth/login');
        } else {
            console.error(response.error);
        };
    };
    return (
        <div className={styles.divContainerRegister}>
            <h3 className={styles.h3RegisterUser}>Crie seu Cadastro</h3>
            <h4 className={styles.h4RegisterUser}>Olá! Preencha seus dados.</h4>
            <form className={styles.formRegisterUser} action={onSubmitForm}>
                <fieldset className={styles.fieldsetCampoEntrada}>
                    <label htmlFor='name' className={styles.labelCampoEntrada}>
                        Apelido
                    </label>
                    <input
                        className={styles.inputCampoEntrada}
                        name="name"
                        id="name"
                        placeholder="Qualquer nome"
                        required
                    />
                </fieldset>
                <fieldset className={styles.fieldsetCampoEntrada}>
                    <label htmlFor='email' className={styles.labelCampoEntrada}>
                        E-mail
                    </label>
                    <input
                        className={styles.inputCampoEntrada}
                        name="email"
                        id="email"
                        type="email"
                        placeholder="Digite seu e-mail"
                        required
                    />
                </fieldset>
                <fieldset className={styles.fieldsetCampoEntrada}>
                    <label htmlFor='password' className={styles.labelCampoEntrada}>
                        Senha
                    </label>
                    <input
                        className={styles.inputCampoEntrada}
                        name="password"
                        id="password"
                        type="password"
                        placeholder='Deve ter:  a-z  A-Z  1234567890  !@#$%&'
                        required
                    />
                </fieldset>
                <button className={styles.btnRegister} type="submit">
                    Cadastrar-se
                </button>
            </form>
            <div className={styles.divRodape}>
                <p>Já tem conta?</p>
                <Link to='/auth/login'>
                    <p className={styles.pRodape}>Faça seu login!</p>
                </Link>
            </div>
        </div>
    )
}
