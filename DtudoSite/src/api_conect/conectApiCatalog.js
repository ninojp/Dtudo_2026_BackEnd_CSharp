import axios from 'axios';

const BFF_BASE_URL = import.meta.env.VITE_BFF_BASE_URL
    || (typeof window !== 'undefined' ? window.location.origin : 'https://homologacao.example.invalid');

export function axiosHttpBffCatalog() {
    return axios.create({
        baseURL: BFF_BASE_URL.replace(/\/+$/, ''),
        withCredentials: true,
        headers: {
            Accept: 'application/json',
        },
    });
}
