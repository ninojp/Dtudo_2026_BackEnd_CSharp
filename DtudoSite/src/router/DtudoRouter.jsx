import { BrowserRouter, Route, Routes, Outlet } from "react-router-dom";
import IndexLayout from "../layouts/IndexLayout/IndexLayout";
import NinoTIPageLayout from "../layouts/NinoTIPageLayout/NinoTIPageLayout";
import MyMusicxObjsListProvider from "../context_api/MyMusicxObjsListContext/MyMusicxObjsListProvider";
import MyMusicXDetalhesProvider from "../context_api/MyMusicXDetalhesContext/MyMusicXDetalhesProvider";
import Register from "../pages/Register/Register";
import Login from "../pages/Login/Login";
import Logout from "../pages/Logout/Logout";
import NotFound from "../pages/NotFound/NotFound";
import Animes from "../pages/Animes/Animes";
import AnimesDetalhes from "../pages/Animes/AnimesDetalhes/AnimesDetalhes";
import AnimesRelacionados from "../pages/Animes/AnimesRelacionados/AnimesRelacionados";
import MyMusicX from "../pages/MyMusicX/MyMusicX";
import MyMusicXBuscar from "../pages/MyMusicX/MyMusicXBuscar/MyMusicXBuscar";
import MyMusicXDetalhes from "../pages/MyMusicX/MyMusicXDetalhes/MyMusicXDetalhes";
import NinoTIIA from "../pages/NinoTI/NinoTIIA/NinoTIIA";
import NinoTIFrontEnd from "../pages/NinoTI/NinoTIFrontEnd/NinoTIFrontEnd";
import NinoTIProgramacao from "../pages/NinoTI/NinoTIProgramacao/NinoTIProgramacao";
import NinoTICyberSecurity from "../pages/NinoTI/NinoTICyberSecurity/NinoTICyberSecurity";
import NinoTIBlockChain from "../pages/NinoTI/NinoTIBlockChain/NinoTIBlockChain";
import NinoTIHardware from "../pages/NinoTI/NinoTIHardware/NinoTIHardware";
import NinoTIOS from "../pages/NinoTI/NinoTIOS/NinoTIOS";
import NinoTIDataScience from "../pages/NinoTI/NinoTIDataScience/NinoTIDataScience";
import NinoTIDesignUX from "../pages/NinoTI/NinoTIDesignUX/NinoTIDesignUX";
import NinoTICienciaComputacao from "../pages/NinoTI/NinoTICienciaComputacao/NinoTICienciaComputacao";
import HTML from "../components/componentsNinoTI/HTML/HTML";
import CSS from "../components/componentsNinoTI/CSS/CSS";
import JavaScript from "../components/componentsNinoTI/JavaScript/JavaScript";
import TypeScript from "../components/componentsNinoTI/TypeScript/TypeScript";
import NodeJS from "../components/componentsNinoTI/NodeJS/NodeJS";
import ReactTech from "../components/componentsNinoTI/React/React";
import Vite from "../components/componentsNinoTI/Vite/Vite";
import NextJS from "../components/componentsNinoTI/NextJS/NextJS";
import Git from "../components/componentsNinoTI/Git/Git";
import GitHub from "../components/componentsNinoTI/GitHub/GitHub";
import Figma from "../components/componentsNinoTI/Figma/Figma";
import WordPress from "../components/componentsNinoTI/WordPress/WordPress";

export default function DtudoRouter() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<IndexLayout />} >
                    <Route index element={<Animes />} />

                    <Route path="/animes">
                        <Route index element={<Animes />} />
                        <Route path="animes-detalhes/:malId" element={<AnimesDetalhes />} />
                        <Route path="animes-relacionados/:malId" element={<AnimesRelacionados />} />
                    </Route>

                    <Route path="/ninoti" element={<NinoTIPageLayout />}>
                        <Route index element={<NinoTIFrontEnd />} />
                        <Route path="front-end" element={<NinoTIFrontEnd />}>
                            <Route path="html5" element={<HTML />} />
                            <Route path="css3" element={<CSS />} />
                            <Route path="javascript" element={<JavaScript />} />
                            <Route path="typescript" element={<TypeScript />} />
                            <Route path="nodejs" element={<NodeJS />} />
                            <Route path="react" element={<ReactTech />} />
                            <Route path="vite" element={<Vite />} />
                            <Route path="nextjs" element={<NextJS />} />
                            <Route path="git" element={<Git />} />
                            <Route path="github" element={<GitHub />} />
                            <Route path="figma" element={<Figma />} />
                            <Route path="wordpress" element={<WordPress />} />
                        </Route>
                        <Route path="programacao" element={<NinoTIProgramacao />} />
                        <Route path="cyber-security" element={<NinoTICyberSecurity />} />
                        <Route path="blockchain" element={<NinoTIBlockChain />} />
                        <Route path="ia" element={<NinoTIIA />} />
                        <Route path="hardware" element={<NinoTIHardware />} />
                        <Route path="os" element={<NinoTIOS />} />
                        <Route path="ciencia-computacao" element={<NinoTICienciaComputacao />} />
                        <Route path="data-science" element={<NinoTIDataScience />} />
                        <Route path="design-ux" element={<NinoTIDesignUX />} />
                    </Route>

                    <Route path="/mymusicx" element={
                        <MyMusicxObjsListProvider>
                            <Outlet />
                        </MyMusicxObjsListProvider>}>
                        <Route index element={<MyMusicX />} />
                        <Route path="mymusicx-buscar" element={<MyMusicXBuscar />} />
                        <Route path="mymusicx-detalhes/:id" element={
                            <MyMusicXDetalhesProvider>
                                <MyMusicXDetalhes />
                            </MyMusicXDetalhesProvider>} />
                    </Route>

                    <Route path='/auth'>
                        <Route path='register' element={<Register />} />
                        <Route path='login' element={<Login />} />
                        <Route path='logout' element={<Logout />} />
                    </Route>

                    <Route path='*' element={<NotFound />} />
                </Route>
            </Routes>
        </BrowserRouter >
    );
};
