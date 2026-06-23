import axios from "axios";
import process from "process";

const API_LOCAL_BASE_URL = process.env.API_LOCAL_BASE_URL || "http://localhost:3666/";

export function axiosHttpRequest() {
    axios.create({
        baseURL: API_LOCAL_BASE_URL,
        headers: {
            "Content-Type": "application/json",
        },
    });
};
//=====================================================
export function axiosHttpApiLocalMyAnimes() {
    axios.create({
        baseURL: process.env.API_LOCAL_MYANIMES_BASE_URL || "https://localhost:63980/",
        headers: {
            "Content-Type": "application/json",
        },
    });
};
