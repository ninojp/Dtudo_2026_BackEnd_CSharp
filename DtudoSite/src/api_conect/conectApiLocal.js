import axios from "axios";

const API_LOCAL_BASE_URL = import.meta.env.VITE_API_LOCAL_BASE_URL || "http://localhost:3666/";
const API_LOCAL_MYANIMES_BASE_URL = import.meta.env.VITE_API_LOCAL_MYANIMES_BASE_URL || "https://localhost:63980/";
const normalizarBaseUrl = (url) => url.replace(/\/+$/, "");
const removerApiLocalDaBase = (url) => normalizarBaseUrl(url).replace(/\/apiLocal$/i, "");

export function axiosHttpRequest() {
    return axios.create({
        baseURL: normalizarBaseUrl(API_LOCAL_BASE_URL),
        headers: {
            "Content-Type": "application/json",
        },
    });
};
//=====================================================
export function axiosHttpApiLocalMyAnimes() {
    return axios.create({
        baseURL: removerApiLocalDaBase(API_LOCAL_MYANIMES_BASE_URL),
        headers: {
            "Content-Type": "application/json",
        },
    });
};
