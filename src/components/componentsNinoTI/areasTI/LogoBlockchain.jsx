import imgLogoBlockchain from './logo-blockchain-bitcoin.webp';

export default function LogoBlockchain({largura, altura}) {
  return (
    <img src={imgLogoBlockchain} alt='Logo BlockChain e Bitcoin' style={{width: largura, height: altura}} />
  )
};
