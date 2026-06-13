import { BrowserRouter, Route, Routes, Outlet } from "react-router-dom";
import ProtetorDeRota from "../components/ProtetorDeRota/ProtetorDeRota";
import IndexLayout from "../layouts/IndexLayout/IndexLayout";
import NinoTIPageLayout from "../layouts/NinoTIPageLayout/NinoTIPageLayout";
import MyAnimesDetalhesProvider from "../context_api/MyAnimesDetalhesContext/MyAnimesDetalhesProvider";
import MyAnimesBuscarJikanProvider from "../context_api/MyAnimesBuscarJikanContext/MyAnimesBuscarJikanProvider";
import MyAnimesObjsListProvider from "../context_api/MyAnimesObjsListContext/MyAnimesObjsListProvider";
import AnimexDetalhesProvider from "../context_api/AnimexDetalhesContext/AnimexDetalhesProvider";
import MyMusicxObjsListProvider from "../context_api/MyMusicxObjsListContext/MyMusicxObjsListProvider";
import MyMusicXDetalhesProvider from "../context_api/MyMusicXDetalhesContext/MyMusicXDetalhesProvider";
import Register from "../pages/Register/Register";
import Login from "../pages/Login/Login";
import Logout from "../pages/Logout/Logout";
import NotFound from "../pages/NotFound/NotFound";
import Animes from "../pages/Animes/Animes";
import MyAnimes from "../pages/MyAnimes/MyAnimes";
import MyAnimesDetalhes from "../pages/MyAnimes/MyAnimesDetalhes/MyAnimesDetalhes";
import MyAnimesBuscar from "../pages/MyAnimes/MyAnimesBuscar/MyAnimesBuscar";
import MyAnimesBuscarDetalhes from "../pages/MyAnimes/MyAnimesBuscarDetalhes/MyAnimesBuscarDetalhes";
import Animex from "../pages/Animex/Animex";
import AnimexDetalhes from "../pages/Animex/AnimexDetalhes/AnimexDetalhes";
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
                    <Route index element={<MyAnimes />} />
                    {/* Rotas para Endereços MyAnimes  */}
                    <Route path="/myanimes">
                        <Route index element={<MyAnimes />} />
                        <Route path="myanimes-detalhes/:slug" element={<MyAnimesDetalhesProvider>
                                <MyAnimesDetalhes />
                            </MyAnimesDetalhesProvider>} />
                        <Route path="myanimes-buscar" element={<MyAnimesBuscarJikanProvider><MyAnimesBuscar /></MyAnimesBuscarJikanProvider>} />
                        <Route path="myanimes-buscar-detalhes" element={<MyAnimesBuscarDetalhes />} />
                    </Route>
                    {/* Rotas para Endereços MyAnimesLista */}
                    <Route path="/animes" element={<Animes />} />
                    {/* Rotas para Endereços Animex */}
                    <Route path='/animex'>
                        <Route index element={<ProtetorDeRota><Animex /></ProtetorDeRota>} />
                        <Route path="animex-detalhes/:slug" element={<ProtetorDeRota>
                                <AnimexDetalhesProvider>
                                    <AnimexDetalhes />
                                </AnimexDetalhesProvider>
                            </ProtetorDeRota>}
                        />
                    </Route>
                    {/* Rotas para Endereços NinoT.I */}
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

                    {/* Rotas para Endereços MyMusicX, passando o contexto */}
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

                    {/* Rotas para Endereços de Autentificação */}
                    <Route path='/auth'>
                        <Route path='register' element={<Register />} />
                        <Route path='login' element={<Login />} />
                        <Route path='logout' element={<Logout />} />
                    </Route>

                    {/* Rotas para Endereços NÃO reconhecidos! */}
                    <Route path='*' element={<NotFound />} />
                </Route>
            </Routes>
        </BrowserRouter >
    );
};
