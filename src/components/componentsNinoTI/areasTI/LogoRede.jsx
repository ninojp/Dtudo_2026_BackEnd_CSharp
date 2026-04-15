import imgLogoRede from './logo-rede.jpg';

export default function LogoRede({largura, altura}) {
  return (
    <img src={imgLogoRede} alt='Logo BlockChain e Bitcoin' style={{width: largura, height: altura}} />
  )
};
