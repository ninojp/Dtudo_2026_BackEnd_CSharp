import { useRef, useEffect } from 'react';
import './ModalDialog.css';
import IconClose from '../icons/IconClose';


export default function ModalDialog({ isOpen=false, onClose, title="Modal título", children }) {
    const modalDialogRef = useRef(null);
    // Sincroniza o estado do modal com a prop isOpen
    useEffect(() => {
        const modalElement = modalDialogRef.current;
        if (!modalElement) return;
        if (isOpen) {
            modalElement.showModal();
        } else {
            modalElement.close();
        }
    }, [isOpen]);

    // Impede o scroll do body quando a modal está aberta
    useEffect(() => {
        if (isOpen) {
            document.body.classList.add('modal-open');
        } else {
            document.body.classList.remove('modal-open');
        }

        // Cleanup: remove a classe quando o componente for desmontado
        return () => {
            document.body.classList.remove('modal-open');
        };
    }, [isOpen]);
    // Handler(manipulador) para click no backdrop (área fora do modal).
    const handleClick = (e) => {
        if (e.target === e.currentTarget) {
            onClose?.();
        }
    };
    //MDN, closedby="any" (fechar modal ao clicar fora), mas necessita testes adicionais
    return (
        <dialog 
            ref={modalDialogRef}
            onClick={handleClick}
            onClose={() => onClose?.()}
        >
            <div className='divTituloModal'>
                <h3>{title}</h3>
                <button 
                    autoFocus
                    className="btnFecharModal" 
                    aria-label="Fechar diálogo modal" 
                    onClick={() => onClose?.()}
                >
                   <IconClose largura={20} altura={20} cor='#ffffff' />
                </button>
            </div>
            <div className='divChildrenModal'>
                {children}
            </div>
        </dialog>
    );
};
