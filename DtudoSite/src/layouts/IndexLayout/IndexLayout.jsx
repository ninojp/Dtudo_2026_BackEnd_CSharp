import { Outlet } from 'react-router-dom'
import NavBarPage from '../../components/NavBarPage/NavBarPage';
import FooterPage from '../../components/FooterPage/FooterPage';

export default function IndexLayout() {
    return (
        <>
            <NavBarPage />
            <Outlet />
            <FooterPage />
        </>
    );
};
