import imgLogoOS from './logo-os.webp';

export default function LogoOS({largura, altura}) {
  return (
    <img src={imgLogoOS} alt='Logos Sistemas Operacionais' style={{width: largura, height: altura, borderRadius: '50%'}}  />
  )
}
