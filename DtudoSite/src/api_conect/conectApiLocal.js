import axios from "axios";

const API_LOCAL_BASE_URL = import.meta.env.VITE_API_LOCAL_BASE_URL || "http://localhost:3666/";
const API_LOCAL_MYANIMES_BASE_URL = import.meta.env.VITE_API_LOCAL_MYANIMES_BASE_URL || "https://localhost:63980/";

export function axiosHttpRequest() {
    return axios.create({
        baseURL: API_LOCAL_BASE_URL,
        headers: {
            "Content-Type": "application/json",
        },
    });
};
//=====================================================
export function axiosHttpApiLocalMyAnimes() {
    return axios.create({
        baseURL: API_LOCAL_MYANIMES_BASE_URL,
        headers: {
            "Content-Type": "application/json",
        },
    });
};
