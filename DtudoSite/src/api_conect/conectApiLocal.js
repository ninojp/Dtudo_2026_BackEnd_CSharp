import axios from "axios";

const API_LOCAL_BASE_URL = import.meta.env.VITE_API_LOCAL_BASE_URL || "http://localhost:3666/";
const BFF_BASE_URL = import.meta.env.VITE_BFF_BASE_URL
    || (typeof window !== 'undefined' ? window.location.origin : 'https://localhost:7120');
const normalizarBaseUrl = (url) => url.replace(/\/+$/, "");

export function axiosHttpRequest() {
    return axios.create({
        baseURL: normalizarBaseUrl(API_LOCAL_BASE_URL),
        headers: {
            "Content-Type": "application/json",
        },
    });
};
//=====================================================
export function axiosHttpBffCatalog() {
    return axios.create({
        baseURL: normalizarBaseUrl(BFF_BASE_URL),
        withCredentials: false,
        headers: {
            "Content-Type": "application/json",
        },
    });
};
