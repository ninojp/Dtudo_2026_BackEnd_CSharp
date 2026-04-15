import styles from './login.module.css'
import { Link, useNavigate } from "react-router"
import { use } from 'react';
import AuthContext from '../../context_api/AuthContext/AuthContext';

export default function Login() {
    const { login } = use(AuthContext);
    const navigate = useNavigate();
    //-----------------------------------------
    const onSubmitFormNewUser = (formData) => {
        const email = formData.get('email');
        const password = formData.get('password');
        const response = login(email, password);
        if (response.success) {
            navigate('/animex')
        } else {
            console.error(response.error)
        }
    };
    //-----------------------------------------
    return (
        <div className={styles.divContainerLogin}>
            <h3 className={styles.h3RegisterUser}>Login</h3>
            <h4 className={styles.h4RegisterUser}>Boas-vindas! Faça seu login.</h4>
            <form className={styles.formLoginUser} action={onSubmitFormNewUser}>
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
                        required
                    />
                </fieldset>
                <fieldset className={styles.fieldsetContainerCheck}>
                    <input
                        className={styles.inputCheckbox}
                        type='checkbox'
                        name='inputCheckbox'
                        id='inputCheckbox'
                    />
                    <label htmlFor='inputCheckbox' className={styles.labelCampoCheckbox}>
                        Lembrar-me
                    </label>
                </fieldset>
                <button className={styles.btnRegister} type="submit">
                    Login
                </button>
            </form>
            <div className={styles.divRodape}>
                <p>Ainda não tem conta?</p>
                <Link to='/auth/register' className={styles.link}>
                    <p className={styles.pRodape}>Crie seu cadastro!</p>
                </Link>
            </div>
        </div>
    );
};
