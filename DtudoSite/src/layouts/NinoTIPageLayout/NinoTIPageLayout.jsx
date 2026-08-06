import { Outlet } from "react-router-dom";
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
