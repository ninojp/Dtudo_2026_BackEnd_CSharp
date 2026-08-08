import { Outlet } from 'react-router-dom';
import CatalogNavBarPage from '../../components/CatalogNavBarPage/CatalogNavBarPage';
import FooterPage from '../../components/FooterPage/FooterPage';

export default function CatalogIndexLayout() {
    return (
        <>
            <CatalogNavBarPage />
            <Outlet />
            <FooterPage />
        </>
    );
}
