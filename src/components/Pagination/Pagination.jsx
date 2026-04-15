// src/components/Pagination/Pagination.jsx
import styles from './Pagination.module.css';

export default function Pagination({ page, totalPages, onPageChange }) {
    const handlePrevious = () => {
        if (page > 1) {
            onPageChange(page - 1);
        }
    };

    const handleNext = () => {
        if (page < totalPages) {
            onPageChange(page + 1);
        }
    };

    if (totalPages <= 1) {
        return null; // Não mostra paginação se houver apenas uma página
    }

    return (
        <div className={styles.paginationContainer}>
            <button onClick={handlePrevious} disabled={page === 1}>
                Anterior
            </button>
            <span>
                Página {page} de {totalPages}
            </span>
            <button onClick={handleNext} disabled={page === totalPages}>
                Próximo
            </button>
        </div>
    );
}
