import styles from './PaginationButtons.module.css';

export default function PaginationButtons({  currentPage, totalPages, onPageChange, isLoading }) {
  /**
   * Manipulador para mudança de página.
   * Rola a janela para o topo e chama a função onPageChange com a nova página.
   * @param {number} newPage - O número da nova página.
   */
  const handlePageChange = (newPage) => {
    if (newPage >= 1 && newPage <= totalPages && !isLoading) {
      window.scrollTo({
        top: 0,
        behavior: 'smooth'
      });
      onPageChange(newPage);
    }
  };

  //===========================================
  return (
    <div className={styles.divContainerBtnPag}>
      <button className={styles.btnPaginacao}
        onClick={() => handlePageChange(1)}
        disabled={currentPage === 1 || isLoading}
        style={{ cursor: currentPage === 1 || isLoading ? 'not-allowed' : 'pointer' }}
      >
        ⏮ Início
      </button>

      <button className={styles.btnPaginacao}
        onClick={() => handlePageChange(currentPage - 1)}
        disabled={currentPage <= 1 || isLoading}
        style={{ cursor: currentPage <= 1 || isLoading ? 'not-allowed' : 'pointer' }}
      >
        ◀ Anterior
      </button>

      <span className={styles.spanTxtPaginacao}>Página</span> {currentPage} <span className={styles.spanTxtPaginacao}>de</span> {totalPages}

      <button className={styles.btnPaginacao}
        onClick={() => handlePageChange(currentPage + 1)}
        disabled={currentPage >= totalPages || isLoading}
        style={{ cursor: currentPage >= totalPages || isLoading ? 'not-allowed' : 'pointer' }}
      >
        Próxima ▶
      </button>

      <button className={styles.btnPaginacao}
        onClick={() => handlePageChange(totalPages)}
        disabled={currentPage === totalPages || isLoading}
        style={{ cursor: currentPage === totalPages || isLoading ? 'not-allowed' : 'pointer' }}
      >
        Última ⏭
      </button>
    </div>
  );
}
