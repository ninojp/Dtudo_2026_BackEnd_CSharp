import { Outlet } from 'react-router'
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
