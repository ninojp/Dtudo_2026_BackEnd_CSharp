import { Outlet } from "react-router";
import AsideNinoTIPage from "../../components/componentsNinoTI/AsideNinoTIPage/AsideNinoTIPage";
import styles from './NinoTIPageLayout.module.css';

export default function NinoTIPageLayout() {
    return (
        <div className={styles.divContainerNinoTILayout}>
            <AsideNinoTIPage/>
            <Outlet />
        </div>
    );
};
