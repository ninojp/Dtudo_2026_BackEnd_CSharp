import { useState, useEffect, useCallback } from 'react';
import MyAnimesBuscarJikanContext from './MyAnimesBuscarJikanContext';

const API_BASE_URL = 'https://localhost:7082/api/anime';

export default function MyAnimesBuscarJikanProvider({ children }) {
    const [searchInput, setSearchInput] = useState('');
    const [currentQuery, setCurrentQuery] = useState('');
    const [currentPage, setCurrentPage] = useState(1);
    const [hasNextPage, setHasNextPage] = useState(false);
    const [totalPages, setTotalPages] = useState(0);
    const [results, setResults] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');
    const [hasSearched, setHasSearched] = useState(false);

    useEffect(() => {
        const controller = new AbortController();

        async function fetchAnimes() {
            if (!currentQuery.trim()) {
                setIsLoading(false);
                setError('');
                setResults([]);
                setHasNextPage(false);
                setTotalPages(0);
                return;
            }

            try {
                setIsLoading(true);
                setError('');
                setResults([]);

                const url = `${API_BASE_URL}/search?q=${encodeURIComponent(currentQuery)}&page=${currentPage}`;
                const response = await fetch(url, { signal: controller.signal });

                if (!response.ok) {
                    throw new Error(`Erro na requisição: ${response.status}`);
                }

                const data = await response.json();
                setResults(data.results ?? []);
                setHasNextPage(Boolean(data.hasNextPage));
                setTotalPages(data.totalPages ?? 0);
                setHasSearched(true);
            } catch (err) {
                if (err.name === 'AbortError') return;
                setError('Erro ao buscar animes. Verifique se a API está rodando.');
                console.error('Erro:', err);
            } finally {
                setIsLoading(false);
            }
        }

        fetchAnimes();

        return () => controller.abort();
    }, [currentQuery, currentPage]);

    const handleSearch = useCallback((query) => {
        const trimmed = query.trim();
        if (!trimmed) {
            setCurrentPage(1);
            setCurrentQuery('');
            setHasSearched(false);
            setResults([]);
            setError('');
            setHasNextPage(false);
            setTotalPages(0);
            return;
        }
        setCurrentPage(1);
        setCurrentQuery(trimmed);
    }, []);

    const handlePrevPage = useCallback(() => {
        setCurrentPage(prev => Math.max(1, prev - 1));
    }, []);

    const handleNextPage = useCallback(() => {
        if (hasNextPage) setCurrentPage(prev => prev + 1);
    }, [hasNextPage]);

    return (
        <MyAnimesBuscarJikanContext.Provider
            value={{
                searchInput,
                setSearchInput,
                currentPage,
                totalPages,
                hasNextPage,
                results,
                isLoading,
                error,
                hasSearched,
                handleSearch,
                handlePrevPage,
                handleNextPage,
            }}
        >
            {children}
        </MyAnimesBuscarJikanContext.Provider>
    );
}
